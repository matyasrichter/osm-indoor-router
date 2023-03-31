<script lang="ts">
	import type { Map, ControlPosition, IControl, LngLat } from 'maplibre-gl';
	import { LngLatBounds, type LngLatLike, MapMouseEvent } from 'maplibre-gl';
	import { Configuration, type RouteNode, RoutingApi, SearchApi } from '../routing-api-client';
	import { onMount } from 'svelte';
	import { Button } from '@svelteuidev/core';
	import { env } from '$env/dynamic/public';

	export let map: Map | undefined;
	export let graphVersion: number;
	export let level: number;
	let container: HTMLDivElement;
	let control: RoutingPickerControl;

	let searchApi: SearchApi;
	let routingApi: RoutingApi;

	let startNode: RouteNode | null = null;
	let targetNode: RouteNode | null = null;

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

	$: if (map !== undefined) {
		map?.addControl(control);
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

	async function route() {
		if (!startNode || !targetNode) {
			return;
		}
		await routingApi
			.routeGet({ from: startNode.id, to: targetNode.id, graphVersion: graphVersion })
			.then((data) => {
				addOrReplaceRoute(data.nodes.map((node) => [node.longitude, node.latitude]));
				zoomToRoute(data.nodes.map((node) => [node.longitude, node.latitude]));
			})
			.catch((error) => console.error(error));
	}

	const addOrReplaceRoute = (coordinates: Array<LngLatLike>) => {
		removeRoute();
		map?.addSource('route', {
			type: 'geojson',
			data: {
				type: 'LineString',
				coordinates: coordinates
			}
		});
		map?.addLayer({
			id: 'route',
			type: 'line',
			source: 'route',
			layout: {
				'line-join': 'round',
				'line-cap': 'round',
				'line-sort-key': 1
			},
			paint: {
				'line-color': '#00b0fb',
				'line-width': 8
			}
		});
	};

	const removeRoute = () => {
		if (map?.getLayer('route')) {
			map?.removeLayer('route');
		}
		if (map?.getSource('route')) {
			map?.removeSource('route');
		}
	};

	const zoomToRoute = (coordinates: Array<LngLatLike>) => {
		// find bounds of the route and fit map to bounds
		const bounds = coordinates.reduce(function (bounds, coordinate) {
			return bounds.extend(coordinate);
		}, new LngLatBounds(coordinates[0], coordinates[0]));

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
		map?.addLayer({
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
		});
	};
	const removeRoutePoint = (name: string) => {
		removeRoute();
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
			<Button disabled={!startNode || !targetNode} on:click={route}>Find route</Button>
		</div>
	</div>
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
</style>
