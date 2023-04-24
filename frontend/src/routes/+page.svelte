<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import type { RoutingConfig } from '../routing-api-client';
	import 'maplibre-gl/dist/maplibre-gl.css';
	import type IndoorEqual from 'mapbox-gl-indoorequal';
	import type { Map } from 'maplibre-gl';
	import maplibre from 'maplibre-gl';
	import RoutingPickerControl from './RoutingPickerControl.svelte';
	import { env } from '$env/dynamic/public';

	const { FullscreenControl, LngLat, LngLatBounds, Map, NavigationControl, ScaleControl } =
		maplibre;

	export let data: RoutingConfig;

	let map: Map;
	let mapContainer: HTMLDivElement;
	let indoorEqual: IndoorEqual;

	onMount(() => {
		map = new Map({
			container: mapContainer,
			// todo: extract key to env
			style: env.PUBLIC_MAPBOX_STYLE_URL!,
			maxBounds: new LngLatBounds(
				new LngLat(data.bbox.southWest.longitude, data.bbox.southWest.latitude),
				new LngLat(data.bbox.northEast.longitude, data.bbox.northEast.latitude)
			)
		});
		map.on('load', () => {
			map?.resize();
		});
		map.addControl(new NavigationControl({}));
		map.addControl(new ScaleControl({}));
		map.addControl(new FullscreenControl({}));
	});

	onDestroy(() => {
		map?.remove();
	});
</script>

<div id="map" bind:this={mapContainer} />
<RoutingPickerControl {map} graphVersion={data.graphVersion} />

<style>
	#map {
		position: absolute;
		top: 0;
		bottom: 0;
		width: 100%;
	}
</style>
