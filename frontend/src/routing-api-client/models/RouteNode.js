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
 * Check if a given object implements the RouteNode interface.
 */
export function instanceOfRouteNode(value) {
	let isInstance = true;
	isInstance = isInstance && 'id' in value;
	isInstance = isInstance && 'latitude' in value;
	isInstance = isInstance && 'longitude' in value;
	isInstance = isInstance && 'level' in value;
	return isInstance;
}
export function RouteNodeFromJSON(json) {
	return RouteNodeFromJSONTyped(json, false);
}
export function RouteNodeFromJSONTyped(json, ignoreDiscriminator) {
	if (json === undefined || json === null) {
		return json;
	}
	return {
		id: json['id'],
		latitude: json['latitude'],
		longitude: json['longitude'],
		level: json['level']
	};
}
export function RouteNodeToJSON(value) {
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
		level: value.level
	};
}
//# sourceMappingURL=RouteNode.js.map
