<script lang="ts">
	let { className = '', children, header } = $props<{ className?: string; children?: import('svelte').Snippet; header?: import('svelte').Snippet }>();
</script>

<section class={`overflow-hidden rounded-[var(--radius-md)] border border-[var(--border)] bg-[var(--bg-surface)] shadow-[var(--shadow-sm)] ${className}`}>
	{#if header}
		<header class="border-b border-[var(--border)] px-5 py-4">
			{@render header()}
		</header>
	{/if}
	<div class="p-5">
		{@render children?.()}
	</div>
</section>
