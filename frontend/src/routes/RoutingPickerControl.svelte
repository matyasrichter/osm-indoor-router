<script lang="ts">
	import type { Map, ControlPosition, IControl, LngLat, LngLatLike } from 'maplibre-gl';
	import { LngLatBounds, MapMouseEvent } from 'maplibre-gl';
	import {
		Configuration,
		type RouteNode,
		RoutingApi,
		SearchApi,
		type Route
	} from '../routing-api-client';
	import { onMount } from 'svelte';
	import { Button } from '@svelteuidev/core';
	import { env } from '$env/dynamic/public';
	import IndoorEqual from 'mapbox-gl-indoorequal';

	export let map: Map | undefined;
	export let graphVersion: number;
	let indoorEqual: IndoorEqual;
	let container: HTMLDivElement;
	let control: RoutingPickerControl;
	let level: number = 0;

	let searchApi: SearchApi;
	let routingApi: RoutingApi;

	let startNode: RouteNode | null = null;
	let targetNode: RouteNode | null = null;

	let route: Route | null = null;

	let pickingStart = false;
	let pickingTarget = false;

	onMount(() => {
		control = new RoutingPickerControl();
		const apiConf = new Configuration({
			basePath: env.PUBLIC_API_URL
		});
		routingApi = new RoutingApi(apiConf);
		searchApi = new SearchApi(apiConf);
	});

	let zIndex1 = 'z-index-1';
	let zIndex2 = 'z-index-2';

	$: if (map !== undefined) {
		map?.on('load', () => {
			indoorEqual = new IndoorEqual(map!, {
				apiKey: env.PUBLIC_INDOOREQUAL_API_KEY,
				heatmap: false
			});
			indoorEqual.loadSprite('/indoorequal/indoorequal');
			indoorEqual?.on('levelchange', (e: string) => {
				try {
					level = parseFloat(e);
				} catch (e) {
					console.error(e);
				}
			});
			map?.addControl(indoorEqual as IControl);
			map?.addSource('empty', {
				type: 'geojson',
				data: { type: 'FeatureCollection', features: [] }
			});
			map?.addControl(control);
			// this is a hack to allow us to order layers later
			// these empty layers will always be available to use as beforeId
			map?.addLayer({
				id: zIndex2,
				type: 'symbol',
				source: 'empty'
			});
			map?.addLayer(
				{
					id: zIndex1,
					type: 'symbol',
					source: 'empty'
				},
				zIndex2
			);
		});
	}

	$: if (map != undefined && startNode != null) {
		addRoutePoint([startNode.longitude, startNode.latitude], 'green', 'start');
	} else {
		removeRoutePoint('start');
	}
	$: if (map != undefined && targetNode != null) {
		addRoutePoint([targetNode.longitude, targetNode.latitude], 'red', 'target');
	} else {
		removeRoutePoint('target');
	}
	$: [route, level] && route != null && addOrReplaceRoute(route.nodes);
	$: route != null && zoomToRoute(route.nodes);

	class RoutingPickerControl implements IControl {
		getDefaultPosition(): ControlPosition {
			return 'top-left';
		}

		onAdd(map: Map): HTMLElement {
			map?.on('click', handleClick);
			return container;
		}

		// eslint-disable-next-line @typescript-eslint/no-unused-vars, @typescript-eslint/no-empty-function
		onRemove(map: Map): void {}
	}

	async function handleClick(e: MapMouseEvent): Promise<void> {
		if (pickingStart) {
			pickingStart = false;
			startNode = await findClosestPoint(e.lngLat);
		} else if (pickingTarget) {
			pickingTarget = false;
			targetNode = await findClosestPoint(e.lngLat);
		}
	}

	async function findClosestPoint(lngLat: LngLat) {
		return await searchApi
			.searchClosestNodeGet({
				longitude: lngLat.lng,
				latitude: lngLat.lat,
				level: level,
				graphVersion: graphVersion
			})
			.catch((error) => {
				console.error(error);
				return Promise.resolve(null);
			});
	}

	async function getRoute() {
		if (!startNode || !targetNode) {
			return;
		}
		await routingApi
			.routeGet({ from: startNode.id, to: targetNode.id, graphVersion: graphVersion })
			.then((data) => {
				route = data;
			})
			.catch((error) => console.error(error));
	}

	const routeLayerName = 'route';
	const routeSourceName = 'route';

	const addOrReplaceRoute = (coordinates: Array<RouteNode>) => {
		removeRoute();
		const segments: { level: number; nodes: RouteNode[] }[] = [];
		let currentSegment: RouteNode[] = [];
		let prev: RouteNode | null = null;
		for (const coord of coordinates) {
			currentSegment.push(coord);
			if (prev?.level && prev?.level !== coord?.level) {
				segments.push({ level: prev.level, nodes: currentSegment });
				currentSegment = [coord];
			}
			prev = coord;
		}
		if (currentSegment.length > 0) {
			segments.push({ level: currentSegment.at(-1)!.level, nodes: currentSegment });
		}

		map?.addSource(routeSourceName, {
			type: 'geojson',
			data: {
				type: 'FeatureCollection',
				features: segments.map((segment) => ({
					type: 'Feature',
					properties: {
						level: segment.level,
						color:
							segment.level === level
								? '#005b96'
								: segment.level < level
								? '#b3cde0'
								: '#011f4b',
						opacity: segment.level === level ? 1 : 0.5
					},
					geometry: {
						type: 'LineString',
						coordinates: segment.nodes.map((node) => [node.longitude, node.latitude])
					}
				}))
			}
		});
		map?.addLayer(
			{
				id: routeLayerName,
				type: 'line',
				source: routeSourceName,
				layout: {
					'line-join': 'round',
					'line-cap': 'round',
					'line-sort-key': ['get', 'level']
				},
				paint: {
					'line-color': ['get', 'color'],
					'line-opacity': ['get', 'opacity'],
					'line-width': 6
				}
			},
			zIndex1
		);
	};

	const removeRoute = () => {
		if (map?.getLayer(routeLayerName)) {
			map?.removeLayer(routeLayerName);
		}
		if (map?.getSource(routeSourceName)) {
			map?.removeSource(routeSourceName);
		}
	};

	const zoomToRoute = (coordinates: RouteNode[]) => {
		if (coordinates.length === 0) {
			return;
		}
		const initial = [coordinates[0].longitude, coordinates[0].latitude];
		// find bounds of the route and fit map to bounds
		const bounds = coordinates.reduce(function (bounds, coordinate) {
			return bounds.extend([coordinate.longitude, coordinate.latitude]);
		}, new LngLatBounds(initial, initial));

		map?.fitBounds(bounds, {
			padding: 100
		});
	};

	const addRoutePoint = (coords: LngLatLike, color: string, name: string) => {
		removeRoutePoint(name);
		map?.addSource(name, {
			type: 'geojson',
			data: {
				type: 'Point',
				coordinates: coords
			}
		});
		map?.addLayer(
			{
				id: name,
				type: 'circle',
				source: name,
				layout: {
					'circle-sort-key': 2
				},
				paint: {
					'circle-radius': ['interpolate', ['exponential', 1.5], ['zoom'], 5, 6, 18, 15],
					'circle-color': color
				}
			},
			zIndex2
		);
	};
	const removeRoutePoint = (name: string) => {
		removeRoute();
		route = null;
		if (map?.getLayer(name)) {
			map?.removeLayer(name);
		}
		if (map?.getSource(name)) {
			map?.removeSource(name);
		}
	};
</script>

<div bind:this={container} class="ctrl">
	<div class="routing-buttons">
		<div>
			<span>Start</span>
			<Button
				color="green"
				override={{
					width: '100px'
				}}
				on:click={() => (pickingStart = !pickingStart)}
				variant={pickingStart ? 'outline' : 'filled'}
			>
				{#if pickingStart}
					Cancel
				{:else}
					Pick
				{/if}
			</Button>
		</div>
		<div>
			<span>Target</span>
			<Button
				color="red"
				override={{
					width: '100px'
				}}
				on:click={() => (pickingTarget = !pickingTarget)}
				variant={pickingTarget ? 'outline' : 'filled'}
			>
				{#if pickingTarget}
					Cancel
				{:else}
					Pick
				{/if}
			</Button>
		</div>
		<div class="submit-button">
			<Button disabled={!startNode || !targetNode} on:click={getRoute}>Find route</Button>
		</div>
	</div>
	{#if route}
		<div class="route-info">
			Found route:
			<div>
				<div>
					<strong>From:</strong> ({route.nodes.at(0)?.longitude}, {route.nodes.at(0)
						?.longitude}), floor: {route.nodes.at(0)?.level}
				</div>
				<div>
					<strong>To:</strong> ({route.nodes.at(-1)?.longitude}, {route.nodes.at(-1)
						?.longitude}), floor: {route.nodes.at(-1)?.level}
				</div>
				<div>
					<strong>Total distance:</strong> {route.totalDistanceInMeters.toFixed(0)} m
				</div>
			</div>
		</div>
	{/if}
</div>

<style lang="scss">
	.ctrl {
		clear: both;
		pointer-events: auto;
		transform: translate(0);
		box-shadow: 0 0 0 2px rgba(0, 0, 0, 0.1);
		background: #fff;
		border-radius: 4px;
		float: left;
		margin: 10px 0 0 10px;
	}

	.routing-buttons {
		background: white;
		padding: 1em;

		> div {
			margin: 0.5em 0;
			height: 3em;
			display: flex;
			justify-content: space-between;

			span:first-child {
				font-size: 2em;
				margin: auto 0.5em auto 0;
			}

			&:first-child {
				margin-top: 0;
			}
			&:last-child {
				margin-bottom: 0;
			}
		}
		.submit-button {
			margin-top: 1em;
			display: flex;
			justify-content: center;
		}
	}
	.route-info {
		padding: 1em;
	}
</style>
