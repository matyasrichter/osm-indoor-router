<script lang="ts">
    import IndoorEqual from "mapbox-gl-indoorequal";
    import {LngLat, LngLatBounds, LngLatLike, Map} from 'maplibre-gl';
    import {onDestroy, onMount} from "svelte";
    import {Configuration, RoutingApi} from "../routing-api-client"

    let map: Map;
    let mapContainer: HTMLDivElement;

    onMount(() => {
        map = new Map({
            container: mapContainer,
            // todo: extract key to env
            style: 'https://api.maptiler.com/maps/basic-v2/style.json?key=VxRJSs3YrShQ27b53YEL',
            maxBounds: new LngLatBounds(
                new LngLat(14.35594439506531, 50.196269582652754),
                new LngLat(14.35934007167816, 50.1981630992081)
            ),
        });
        map.once('load', function () {
            map.resize();
        });
        map.on('mousemove', function (e) {
            document.getElementById('info').innerHTML =
                // e.point is the x, y coordinates of the mousemove event relative
                // to the top-left corner of the map
                JSON.stringify(e.point) +
                '<br />' +
                // e.lngLat is the longitude, latitude geographical position of the event
                JSON.stringify(e.lngLat.wrap());
        });

        // todo: extract key to env
        const indoorEqual = new IndoorEqual(map, {apiKey: 'iek_jSa6tSF1g0wUbRUL1iLsIq4R0gMJ'});
        map.addControl(indoorEqual);
    });

    onDestroy(() => {
        if (!!map) {
            map.remove();
        }
    });

    $: from = 0 as bigint;
    $: to = 0 as bigint;
    $: version = 0 as bigint;

    function addOrReplaceRoute(map: Map, coordinates: Array<LngLatLike>) {
        if (!!map.getLayer('route')) {
            map.removeLayer('route');
        }
        if (!!map.getSource('route')) {
            map.removeSource('route');
        }
        map.addSource('route', {
            type: 'geojson',
            data: {
                'type': 'LineString',
                'coordinates': coordinates
            }
        })
        map.addLayer({
            'id': 'route',
            'type': 'line',
            'source': 'route',
            'layout': {
                'line-join': 'round',
                'line-cap': 'round'
            },
            'paint': {
                'line-color': 'red',
                'line-width': 8
            }
        });

        // find bounds of the route and fit map to bounds
        const bounds = coordinates.reduce(function (bounds, coord) {
            return bounds.extend(coord);
        }, new LngLatBounds(coordinates[0], coordinates[0]));

        map.fitBounds(bounds, {
            padding: 100
        });
    }

    async function route() {
        await new RoutingApi(new Configuration({
            basePath: "http://localhost:5276"
        })).routeGet({from: from, to: to, graphVersion: version})
            .then(data => {
                addOrReplaceRoute(
                    map,
                    data.nodes.map(node => [node.longitude, node.latitude])
                );
            })
            .catch(error => console.error(error));
    }
</script>

<div class="wrapper">
    <form on:submit|preventDefault={route}>
        <label>
            From:
            <input type="text" bind:value={from}>
        </label>
        <label>
            To:
            <input type="text" bind:value={to}>
        </label>
        <label>
            Version:
            <input type="text" bind:value={version}>
        </label>
        <button type="submit">Route</button>
    </form>
    <div id="info"></div>
    <div id="map" bind:this={mapContainer}></div>
</div>
<style>
    @import "maplibre-gl/dist/maplibre-gl.css";

    .wrapper {
        display: flex;
        flex-direction: column;
        height: 100vh;
    }

    #map {
        flex-grow: 1;
        width: 100%;
    }

</style>
