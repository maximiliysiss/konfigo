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
		onclick,
		children
	} = $props<{
		type?: 'button' | 'submit' | 'reset';
		variant?: Variant;
		size?: Size;
		disabled?: boolean;
		loading?: boolean;
		className?: string;
		onclick?: (event: MouseEvent) => void;
		children?: import('svelte').Snippet;
	}>();

	const base =
		'inline-flex items-center justify-center gap-2 rounded-[8px] border text-[13px] font-medium transition-all duration-150 ease-out active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-40';

	const sizes: Record<Size, string> = {
		sm: 'h-7 px-3',
		md: 'h-[34px] px-3.5',
		lg: 'h-10 px-4'
	};

	const variants: Record<Variant, string> = {
		primary: 'border-transparent bg-[var(--accent)] text-white hover:bg-[var(--accent-hover)]',
		secondary: 'border-[var(--border)] bg-[var(--bg-surface)] text-[var(--text-primary)] hover:bg-[var(--bg-subtle)]',
		ghost: 'border-transparent bg-transparent text-[var(--text-secondary)] hover:bg-[var(--bg-subtle)] hover:text-[var(--text-primary)]',
		danger: 'border-transparent bg-[var(--danger)] text-white hover:bg-[color:color-mix(in_srgb,var(--danger)_88%,black)]'
	};
</script>

<button class={`${base} ${sizes[size as Size]} ${variants[variant as Variant]} ${className}`} {type} {disabled} onclick={onclick}>
	{#if loading}
		<Spinner size={size === 'lg' ? 20 : 16} />
	{/if}
	<span>{@render children?.()}</span>
</button>
