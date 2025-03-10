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
import type { RouteNode } from './RouteNode';
import { RouteNodeFromJSON, RouteNodeFromJSONTyped, RouteNodeToJSON } from './RouteNode';

/**
 *
 * @export
 * @interface Route
 */
export interface Route {
	/**
	 *
	 * @type {Array<RouteNode>}
	 * @memberof Route
	 */
	nodes: Array<RouteNode>;
	/**
	 *
	 * @type {number}
	 * @memberof Route
	 */
	totalDistanceInMeters: number;
}

/**
 * Check if a given object implements the Route interface.
 */
export function instanceOfRoute(value: object): boolean {
	let isInstance = true;
	isInstance = isInstance && 'nodes' in value;
	isInstance = isInstance && 'totalDistanceInMeters' in value;

	return isInstance;
}

export function RouteFromJSON(json: any): Route {
	return RouteFromJSONTyped(json, false);
}

export function RouteFromJSONTyped(json: any, ignoreDiscriminator: boolean): Route {
	if (json === undefined || json === null) {
		return json;
	}
	return {
		nodes: (json['nodes'] as Array<any>).map(RouteNodeFromJSON),
		totalDistanceInMeters: json['totalDistanceInMeters']
	};
}

export function RouteToJSON(value?: Route | null): any {
	if (value === undefined) {
		return undefined;
	}
	if (value === null) {
		return null;
	}
	return {
		nodes: (value.nodes as Array<any>).map(RouteNodeToJSON),
		totalDistanceInMeters: value.totalDistanceInMeters
	};
}
