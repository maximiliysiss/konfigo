<script lang="ts">
	import Spinner from './Spinner.svelte';

	type Variant = 'primary' | 'secondary' | 'ghost' | 'danger';
	type Size = 'sm' | 'md' | 'lg';

	let {
		type = 'button',
		variant = 'primary',
		size = 'md',
		disabled = false,
		loading = false,
		className = '',
		ariaLabel = undefined,
		onclick,
		children
	} = $props<{
		type?: 'button' | 'submit' | 'reset';
		variant?: Variant;
		size?: Size;
		disabled?: boolean;
		loading?: boolean;
		className?: string;
		ariaLabel?: string;
		onclick?: (event: MouseEvent) => void;
		children?: import('svelte').Snippet;
	}>();

	const base =
		'inline-flex items-center justify-center gap-2 rounded-[var(--radius-sm)] border text-[13px] font-semibold transition-colors duration-150 ease-out disabled:cursor-not-allowed disabled:opacity-45';

	const sizes: Record<Size, string> = {
		sm: 'h-7 px-3',
		md: 'h-[34px] px-3.5',
		lg: 'h-10 px-4'
	};

	const variants: Record<Variant, string> = {
		primary: 'border-[var(--accent)] bg-[var(--accent)] text-white hover:border-[var(--accent-hover)] hover:bg-[var(--accent-hover)]',
		secondary: 'border-[var(--border-strong)] bg-[var(--bg-elevated)] text-[var(--text-primary)] hover:border-[var(--accent)]',
		ghost: 'border-transparent bg-transparent text-[var(--text-secondary)] hover:bg-[var(--bg-subtle)] hover:text-[var(--text-primary)]',
		danger: 'border-transparent bg-[var(--danger)] text-white hover:bg-[color:color-mix(in_srgb,var(--danger)_88%,black)]'
	};
</script>

<button class={`${base} ${sizes[size as Size]} ${variants[variant as Variant]} ${className}`} {type} {disabled} aria-label={ariaLabel} onclick={onclick}>
	{#if loading}
		<Spinner size={size === 'lg' ? 20 : 16} />
	{/if}
	<span>{@render children?.()}</span>
</button>
