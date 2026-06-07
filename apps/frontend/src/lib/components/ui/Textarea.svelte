<script lang="ts">
	let {
		id = '',
		label = '',
		placeholder = '',
		value = $bindable(''),
		error = '',
		disabled = false,
		readonly = false,
		className = '',
		oninput
	} = $props<{
		id?: string;
		label?: string;
		placeholder?: string;
		value?: string;
		error?: string;
		disabled?: boolean;
		readonly?: boolean;
		className?: string;
		oninput?: (event: Event) => void;
	}>();
</script>

<label class={`block space-y-1.5 ${className}`}>
	{#if label}
		<span class="block text-[13px] text-[var(--text-secondary)]">{label}</span>
	{/if}
	<textarea
		class={`min-h-20 w-full resize-y rounded-[7px] border bg-[var(--bg-elevated)] px-3 py-2 text-[14px] text-[var(--text-primary)] outline-none transition-all duration-150 placeholder:text-[var(--text-tertiary)] ${
			error ? 'border-[var(--danger)]' : 'border-[var(--border)] focus:border-[var(--ink)]'
		}`}
		{id}
		{placeholder}
		{disabled}
		{readonly}
		bind:value
		{oninput}
	></textarea>
	{#if error}
		<span class="block text-[12px] text-[var(--danger)]">{error}</span>
	{/if}
</label>
