<script lang="ts">
	let {
		id = '',
		label = '',
		type = 'text',
		placeholder = '',
		value = $bindable(''),
		error = '',
		disabled = false,
		readonly = false,
		list,
		className = '',
		oninput,
		onkeydown,
		prefix
	} = $props<{
		id?: string;
		label?: string;
		type?: string;
		placeholder?: string;
		value?: string;
		error?: string;
		disabled?: boolean;
		readonly?: boolean;
		list?: string;
		className?: string;
		oninput?: (event: Event) => void;
		onkeydown?: (event: KeyboardEvent) => void;
		prefix?: import('svelte').Snippet;
	}>();
</script>

<label class={`block space-y-1.5 ${className}`}>
	{#if label}
		<span class="block text-[13px] text-[var(--text-secondary)]">{label}</span>
	{/if}
	<span
		class={`flex h-9 items-center gap-2 rounded-[8px] border bg-[var(--bg-surface)] px-3 transition-all duration-150 ${
			error ? 'border-[var(--danger)]' : 'border-[var(--border)] focus-within:border-[var(--accent)]'
		}`}
	>
		{#if prefix}
			<span class="text-[var(--text-tertiary)]">{@render prefix()}</span>
		{/if}
		<input
			class="w-full border-0 bg-transparent p-0 text-[14px] text-[var(--text-primary)] outline-none placeholder:text-[var(--text-tertiary)]"
			{id}
			{type}
			{placeholder}
			{disabled}
			{readonly}
			{list}
			bind:value
			{oninput}
			{onkeydown}
		/>
	</span>
	{#if error}
		<span class="block text-[12px] text-[var(--danger)]">{error}</span>
	{/if}
</label>
