<script lang="ts">
	let {
		id = '',
		label = '',
		value = $bindable(''),
		error = '',
		disabled = false,
		className = '',
		children
	} = $props<{
		id?: string;
		label?: string;
		value?: string | number;
		error?: string;
		disabled?: boolean;
		className?: string;
		children?: import('svelte').Snippet;
	}>();
</script>

<label class={`block space-y-1.5 ${className}`}>
	{#if label}
		<span class="block text-[13px] text-[var(--text-secondary)]">{label}</span>
	{/if}
	<span
		class={`relative flex h-9 items-center rounded-[8px] border bg-[var(--bg-surface)] px-3 transition-all duration-150 ${
			error ? 'border-[var(--danger)]' : 'border-[var(--border)] focus-within:border-[var(--accent)]'
		}`}
	>
		<select
			class="h-full w-full appearance-none border-0 bg-transparent pr-6 text-[14px] text-[var(--text-primary)] outline-none"
			{id}
			{disabled}
			bind:value
		>
			{@render children?.()}
		</select>
		<svg class="pointer-events-none absolute right-3 h-4 w-4 text-[var(--text-tertiary)]" viewBox="0 0 16 16" fill="none">
			<path d="M4 6l4 4 4-4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round" />
		</svg>
	</span>
	{#if error}
		<span class="block text-[12px] text-[var(--danger)]">{error}</span>
	{/if}
</label>
