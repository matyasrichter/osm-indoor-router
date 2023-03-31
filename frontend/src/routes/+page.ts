import { ConfigApi, Configuration, type RoutingConfig } from '../routing-api-client';
import { error } from '@sveltejs/kit';

export async function load({ fetch }): Promise<RoutingConfig> {
	const api = new ConfigApi(
		new Configuration({
			fetchApi: fetch,
			basePath: 'http://localhost:5276'
		})
	);

	return await api.configGet().catch((e) => {
		console.error(e);
		throw error(500, 'Something went wrong.');
	});
}
