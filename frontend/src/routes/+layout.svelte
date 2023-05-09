<script>
	import 'normalize.css';
    import { SvelteToast } from '@zerodevx/svelte-toast'
</script>

<svelte:head>
	<title>Indoor Router</title>
</svelte:head>
<div class="wrapper">
	<slot />
    <SvelteToast />
</div>

<style>
	.wrapper {
		width: 100vw;
		height: 100vh;
	}
</style>
