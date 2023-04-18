import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';
import { viteStaticCopy } from 'vite-plugin-static-copy';

export default defineConfig({
	plugins: [
		// @ts-ignore
		sveltekit(),
		// @ts-ignore
		viteStaticCopy({
			targets: [
				{
					src: './node_modules/mapbox-gl-indoorequal/sprite/*',
					dest: 'indoorequal'
				}
			]
		})
	],
	test: {
		include: ['src/**/*.{test,spec}.{js,ts}']
	},
	server: {
		fs: {
			allow: ['..']
		}
	}
});
