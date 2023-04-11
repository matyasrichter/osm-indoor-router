import type { Map } from 'maplibre-gl';

declare module 'mapbox-gl-indoorequal';
/**
 * Load the indoor= source and layers in your map.
 * @param {object} map the mapbox-gl/maplibre-gl instance of the map
 * @param {IndoorEqualOpts} options
 * @param {string} [options.url] Override the default tiles URL (https://tiles.indoorequal.org/).
 * @param {object} [options.geojson] GeoJSON data with with key as layer name and value with geojson features
 * @param {string} [options.apiKey] The API key if you use the default tile URL (get your free key at [indoorequal.com](https://indoorequal.com)).
 * @param {array} [options.layers] The layers to be used to style indoor= tiles. Take a look a the [layers.js file](https://github.com/indoorequal/mapbox-gl-indoorequal/blob/master/src/layers.js) file and the [vector schema](https://indoorequal.com/schema)
 * @param {boolean} [options.heatmap] Should the heatmap layer be visible at start (true : visible, false : hidden). Defaults to true/visible.
 * @property {string} level The current level displayed
 * @property {array} levels  The levels that can be displayed in the current view
 * @fires IndoorEqual#levelschange
 * @fires IndoorEqual#levelchange
 * @return {IndoorEqual} `this`
 */
export default class IndoorEqual {
	constructor(map: Map, options?: IndoorEqualOpts);
	source: GeoJSONSource | VectorTileSource;
	map: Map;
	levels: string[];
	level: string;
	events: {};
	/**
	 * Remove any layers, source and listeners and controls
	 */
	remove(): void;
	/**
	 * Add an event listener
	 * @param {string} name the name of the event
	 * @param {function} fn the function to be called when the event is emitted
	 */
	on(name: string, fn: Function): void;
	/**
	 * Remove an event listener.
	 * @param {string} name the name of the event
	 * @param {function} fn the same function when on() was called
	 */
	off(name: string, fn: Function): void;
	/**
	 * Add the level control to the map
	 * Used when adding the control via the map instance: map.addControl(indoorEqual)
	 */
	onAdd(): any;
	_control: any;
	/**
	 * Remove the level control
	 * Used when removing the control via the map instance: map.removeControl(indoorEqual)
	 */
	onRemove(): void;
	/**
	 * Set the displayed level.
	 * @param {string} level the level to be displayed
	 * @fires IndoorEqual#levelchange
	 */
	setLevel(level: string): void;
	/**
	 * Set the displayed level.
	 * @deprecated Use setLevel instead
	 * @param {string} level the level to be displayed
	 * @fires IndoorEqual#levelchange
	 */
	updateLevel(level: string): void;
	/**
	 * Load a sprite and add all images to the map
	 * @param {string} baseUrl the baseUrl where to load the sprite
	 * @param {object} options
	 * @param {boolean} [options.update] Update existing image (default false)
	 * @return {Promise} It resolves an hash of images.
	 */
	loadSprite(
		baseUrl: string,
		options?: {
			update?: boolean;
		}
	): Promise<any>;
	/**
	 * Change the heatmap layer visibility
	 * @param {boolean} visible True to make it visible, false to hide it
	 */
	setHeatmapVisible(visible: boolean): void;
	_init(): void;
	_updateLevelsDebounce: any;
	_updateFilters(): void;
	_refreshAfterLevelsUpdate(): void;
	_updateLevels(): void;
	_emitLevelsChange(): void;
	_emitLevelChange(): void;
	_emitEvent(eventName: any, ...args: any[]): void;
}
declare class GeoJSONSource {
	constructor(map: Map, options?: {});
	map: Map;
	geojson: {};
	layers: any;
	baseSourceId: string;
	sourceId: string;
	addSource(): void;
	addLayers(): void;
	remove(): void;
}
declare class VectorTileSource {
	constructor(map: Map, options?: {});
	map: Map;
	url: string;
	apiKey: any;
	layers: any;
	sourceId: string;
	addSource(): void;
	addLayers(): void;
	remove(): void;
}
declare interface IndoorEqualOpts {
	url?: string;
	geojson?: object;
	apiKey?: string;
	layers?: any[];
	heatmap?: boolean;
}
export {};
