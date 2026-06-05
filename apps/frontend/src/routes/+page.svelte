<script lang="ts">
	import Button from '$lib/components/ui/Button.svelte';
</script>

<section class="flex min-h-[70vh] items-center justify-center">
	<div class="max-w-xl text-center">
		<p class="section-label">Control plane</p>
		<h1 class="mt-4 text-[32px] font-semibold">Manage service configuration with live rollout visibility.</h1>
		<p class="mt-4 text-[16px] text-[var(--text-secondary)]">Browse services, version configuration safely, and react to live changes in one place.</p>
		<div class="mt-8 flex justify-center">
			<a href="/services">
				<Button size="lg">Open services</Button>
			</a>
		</div>
	</div>
</section>
