/* tslint:disable */
/* eslint-disable */
/**
 * API
 * No description provided (generated by Openapi Generator https://github.com/openapitools/openapi-generator)
 *
 * The version of the OpenAPI document: 1.0
 *
 *
 * NOTE: This class is auto generated by OpenAPI Generator (https://openapi-generator.tech).
 * https://openapi-generator.tech
 * Do not edit the class manually.
 */

import { exists, mapValues } from '../runtime';
/**
 *
 * @export
 * @interface RouteNode
 */
export interface RouteNode {
	/**
	 *
	 * @type {number}
	 * @memberof RouteNode
	 */
	id: number;
	/**
	 *
	 * @type {number}
	 * @memberof RouteNode
	 */
	latitude: number;
	/**
	 *
	 * @type {number}
	 * @memberof RouteNode
	 */
	longitude: number;
	/**
	 *
	 * @type {number}
	 * @memberof RouteNode
	 */
	level: number;
	/**
	 *
	 * @type {boolean}
	 * @memberof RouteNode
	 */
	isLevelConnection: boolean;
}

/**
 * Check if a given object implements the RouteNode interface.
 */
export function instanceOfRouteNode(value: object): boolean {
	let isInstance = true;
	isInstance = isInstance && 'id' in value;
	isInstance = isInstance && 'latitude' in value;
	isInstance = isInstance && 'longitude' in value;
	isInstance = isInstance && 'level' in value;
	isInstance = isInstance && 'isLevelConnection' in value;

	return isInstance;
}

export function RouteNodeFromJSON(json: any): RouteNode {
	return RouteNodeFromJSONTyped(json, false);
}

export function RouteNodeFromJSONTyped(json: any, ignoreDiscriminator: boolean): RouteNode {
	if (json === undefined || json === null) {
		return json;
	}
	return {
		id: json['id'],
		latitude: json['latitude'],
		longitude: json['longitude'],
		level: json['level'],
		isLevelConnection: json['isLevelConnection']
	};
}

export function RouteNodeToJSON(value?: RouteNode | null): any {
	if (value === undefined) {
		return undefined;
	}
	if (value === null) {
		return null;
	}
	return {
		id: value.id,
		latitude: value.latitude,
		longitude: value.longitude,
		level: value.level,
		isLevelConnection: value.isLevelConnection
	};
}
