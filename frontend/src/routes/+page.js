import { ConfigApi, Configuration } from '../routing-api-client';
import { error } from '@sveltejs/kit';
import { env } from '$env/dynamic/public';
export async function load({ fetch }) {
	const api = new ConfigApi(
		new Configuration({
			fetchApi: fetch,
			basePath: env.PUBLIC_API_URL
		})
	);
	return await api.configGet().catch((e) => {
		console.error(e);
		throw error(500, 'Something went wrong.');
	});
}
//# sourceMappingURL=+page.js.map
