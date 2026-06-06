<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
</script>

<section class="grid min-h-[70vh] place-items-center">
	<div class="w-full max-w-[720px] border-l-2 border-[var(--ink)] pl-6">
		<p class="section-label">Control plane</p>
		<h1 class="mt-4 text-[40px] font-semibold leading-tight text-[var(--text-primary)]">Konfigo keeps runtime configuration readable, versioned, and live.</h1>
		<p class="mt-5 max-w-[560px] text-[16px] text-[var(--text-secondary)]">Browse services, inspect ownership, ship config versions, and watch changes land from a focused operations surface.</p>
		<div class="mt-8 flex">
			<a href="/services">
				<Button size="lg">Open services</Button>
			</a>
		</div>
	</div>
</section>
