<script lang="ts">
    import {onDestroy, onMount} from "svelte";
    import type {RoutingConfig} from "../routing-api-client"
    import 'maplibre-gl/dist/maplibre-gl.css';
    import IndoorEqual from 'mapbox-gl-indoorequal'
    import {FullscreenControl, IControl, LngLat, LngLatBounds, Map, NavigationControl, ScaleControl} from 'maplibre-gl'
    import RoutingPickerControl from "./RoutingPickerControl.svelte";

    export let data: RoutingConfig;

    let map: Map;
    let mapContainer: HTMLDivElement;
    let indoorEqual: IndoorEqual;

    onMount(() => {
        map = new Map({
            container: mapContainer,
            // todo: extract key to env
            style: 'https://api.maptiler.com/maps/basic-v2/style.json?key=VxRJSs3YrShQ27b53YEL',
            maxBounds: new LngLatBounds(
                new LngLat(data.bbox.southWest.longitude, data.bbox.southWest.latitude),
                new LngLat(data.bbox.northEast.longitude, data.bbox.northEast.latitude)
            ),
        });
        map.on('load', () => {
            map?.resize();
        });

        // todo: extract key to env
        indoorEqual = new IndoorEqual(map, {apiKey: 'iek_jSa6tSF1g0wUbRUL1iLsIq4R0gMJ'});
        map.addControl(indoorEqual as IControl);
        map.addControl(new NavigationControl({}))
        map.addControl(new ScaleControl({}))
        map.addControl(new FullscreenControl({}))
    });

    onDestroy(() => {
        map?.remove()
    });
</script>

<div id="map" bind:this={mapContainer}></div>
<RoutingPickerControl
    map={map}
    graphVersion={data.graphVersion}
    level={indoorEqual?.level}
></RoutingPickerControl>

<style>
    #map {
        position: absolute;
        top: 0;
        bottom: 0;
        width: 100%;
    }
</style>
