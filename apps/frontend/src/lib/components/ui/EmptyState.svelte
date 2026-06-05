<script lang="ts">
	let { title = '', description = '', icon, action } = $props<{ title?: string; description?: string; icon?: import('svelte').Snippet; action?: import('svelte').Snippet }>();
</script>

<div class="flex flex-col items-center justify-center py-[200px] text-center">
	<div class="mb-4 text-[var(--text-tertiary)]">
		{@render icon?.()}
	</div>
	<h2 class="text-[20px] font-semibold">{title}</h2>
	<p class="mt-2 max-w-md text-[14px] text-[var(--text-secondary)]">{description}</p>
	{#if action}
		<div class="mt-6">
			{@render action()}
		</div>
	{/if}
</div>
