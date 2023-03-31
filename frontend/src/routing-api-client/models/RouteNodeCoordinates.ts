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
 * @interface RouteNodeCoordinates
 */
export interface RouteNodeCoordinates {
	/**
	 *
	 * @type {number}
	 * @memberof RouteNodeCoordinates
	 */
	latitude: number;
	/**
	 *
	 * @type {number}
	 * @memberof RouteNodeCoordinates
	 */
	longitude: number;
}

/**
 * Check if a given object implements the RouteNodeCoordinates interface.
 */
export function instanceOfRouteNodeCoordinates(value: object): boolean {
	let isInstance = true;
	isInstance = isInstance && 'latitude' in value;
	isInstance = isInstance && 'longitude' in value;

	return isInstance;
}

export function RouteNodeCoordinatesFromJSON(json: any): RouteNodeCoordinates {
	return RouteNodeCoordinatesFromJSONTyped(json, false);
}

export function RouteNodeCoordinatesFromJSONTyped(
	json: any,
	ignoreDiscriminator: boolean
): RouteNodeCoordinates {
	if (json === undefined || json === null) {
		return json;
	}
	return {
		latitude: json['latitude'],
		longitude: json['longitude']
	};
}

export function RouteNodeCoordinatesToJSON(value?: RouteNodeCoordinates | null): any {
	if (value === undefined) {
		return undefined;
	}
	if (value === null) {
		return null;
	}
	return {
		latitude: value.latitude,
		longitude: value.longitude
	};
}
