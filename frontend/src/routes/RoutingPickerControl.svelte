<script lang="ts">
    import type {Map, ControlPosition, IControl, LngLat} from 'maplibre-gl'
    import {LngLatBounds, type LngLatLike, MapMouseEvent} from 'maplibre-gl'
    import {Configuration, type RouteNode, RoutingApi, SearchApi} from "../routing-api-client";
    import {onMount} from 'svelte'
    import {Button} from '@svelteuidev/core'

    export let map: Map | undefined
    export let graphVersion: number
    export let level: number
    let container: HTMLDivElement
    let control: RoutingPickerControl

    let searchApi: SearchApi
    let routingApi: RoutingApi

    let start: RouteNode | null = null
    let target: RouteNode | null = null

    let pickingStart = false
    let pickingTarget = false

    onMount(() => {
        control = new RoutingPickerControl()
        const apiConf = new Configuration({
            basePath: "http://localhost:5276"
        })
        routingApi = new RoutingApi(apiConf)
        searchApi = new SearchApi(apiConf)
    })

    $: {
        if (map !== undefined) {
            map?.addControl(control)
        }
    }

    class RoutingPickerControl implements IControl {
        getDefaultPosition(): ControlPosition {
            return "top-left"
        }

        onAdd(map: Map): HTMLElement {
            map?.on('click', handleClick)
            return container
        }

        onRemove(map: Map): void {
            container.remove()
        }
    }

    async function handleClick(e: MapMouseEvent): Promise<void> {
        if (pickingStart) {
            pickingStart = false
            start = await findClosestPoint(e.lngLat)
        } else if (pickingTarget) {
            pickingTarget = false
            target = await findClosestPoint(e.lngLat)
        }
    }

    async function findClosestPoint(lngLat: LngLat) {
        return await searchApi
            .searchClosestNodeGet({
                longitude: lngLat.lng,
                latitude: lngLat.lat,
                level: level,
                graphVersion: graphVersion,
            })
            .catch(error => {
                console.error(error)
                return Promise.resolve(null)
            });
    }


    async function route() {
        if (!start || !target) {
            return
        }
        await routingApi
            .routeGet({from: start.id, to: target.id, graphVersion: graphVersion})
            .then(data => {
                addOrReplaceRoute(
                    data.nodes.map(node => [node.longitude, node.latitude])
                );
            })
            .catch(error => console.error(error));
    }

    const addOrReplaceRoute = (coordinates: Array<LngLatLike>) => {
        if (map?.getLayer('route')) {
            map?.removeLayer('route');
        }
        if (map?.getLayer('start-end')) {
            map?.removeLayer('start-end');
        }
        if (map?.getSource('route')) {
            map?.removeSource('route');
        }
        if (map?.getSource('start-end')) {
            map?.removeSource('start-end');
        }
        map?.addSource('route', {
            type: 'geojson',
            data: {
                'type': 'LineString',
                'coordinates': coordinates
            }
        })
        map?.addLayer({
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
        map?.addSource('start-end', {
            type: "geojson",
            data: {
                type: "MultiPoint",
                coordinates: [
                    coordinates[0],
                    coordinates[coordinates.length - 1],
                ]
            }
        })
        map?.addLayer({
            id: 'start-end',
            type: 'circle',
            source: 'start-end',
            paint: {
                'circle-radius': [
                    'interpolate',
                    ['exponential', 1.5],
                    ['zoom'],
                    5, 6,
                    18, 15
                ],
                'circle-color': 'red',
                'circle-stroke-width': 3,
                'circle-stroke-color': 'red'
            }
        })

        // find bounds of the route and fit map to bounds
        const bounds = coordinates.reduce(function (bounds, coordinate) {
            return bounds.extend(coordinate);
        }, new LngLatBounds(coordinates[0], coordinates[0]));

        map?.fitBounds(bounds, {
            padding: 100
        });
    }
</script>

<div bind:this={container} class="ctrl">
    <div class="routing-buttons">
        <Button color="green"
                on:click={() => pickingStart = !pickingStart}
                variant={pickingStart ? "outline" : "filled"}>
            Pick start
        </Button>
        <Button color="red"
                on:click={() => pickingTarget = !pickingTarget}
                variant={pickingTarget ? "outline" : "filled"}>
            Pick target
        </Button>
        <Button disabled={!start || !target}
        on:click={route}>
            Find route
        </Button>
    </div>
</div>

<style>
    .ctrl {
        clear: both;
        pointer-events: auto;
        transform: translate(0);
        box-shadow: 0 0 0 2px rgba(0, 0, 0, .1);
        background: #fff;
        border-radius: 4px;
        float: left;
        margin: 10px 0 0 10px;
    }

    .routing-buttons {
        background: white;
        padding: 1em;
    }
</style>

