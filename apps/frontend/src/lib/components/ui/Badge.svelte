<script lang="ts">
	type Variant = 'default' | 'success' | 'warning' | 'danger' | 'accent';
	let { variant = 'default', className = '', children } = $props<{ variant?: Variant; className?: string; children?: import('svelte').Snippet }>();

	const variants: Record<Variant, string> = {
		default: 'bg-[var(--bg-subtle)] text-[var(--text-secondary)]',
		success: 'bg-[var(--success-subtle)] text-[var(--success)]',
		warning: 'bg-[var(--warning-subtle)] text-[var(--warning)]',
		danger: 'bg-[var(--danger-subtle)] text-[var(--danger)]',
		accent: 'bg-[var(--accent-subtle)] text-[var(--accent-text)]'
	};
</script>

<span class={`inline-flex items-center rounded-[20px] px-2 py-1 text-[11px] uppercase tracking-[0.12em] ${variants[variant as Variant]} ${className}`}>
	{@render children?.()}
</span>
