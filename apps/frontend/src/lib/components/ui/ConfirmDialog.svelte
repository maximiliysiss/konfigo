<script lang="ts">
	import Button from './Button.svelte';

	let {
		open = false,
		title = 'Confirm',
		message = '',
		confirmLabel = 'Confirm',
		cancelLabel = 'Cancel',
		loading = false,
		onconfirm,
		oncancel
	} = $props<{
		open?: boolean;
		title?: string;
		message?: string;
		confirmLabel?: string;
		cancelLabel?: string;
		loading?: boolean;
		onconfirm?: () => void;
		oncancel?: () => void;
	}>();
</script>

{#if open}
	<div class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4 backdrop-blur-[4px]">
		<div class="w-full max-w-[400px] rounded-[16px] border border-[var(--border)] bg-[var(--bg-surface)] p-6 shadow-[var(--shadow-md)]">
			<h3 class="text-[20px] font-semibold">{title}</h3>
			<p class="mt-3 text-[14px] text-[var(--text-secondary)]">{message}</p>
			<div class="mt-6 flex justify-end gap-3">
				<Button variant="ghost" onclick={oncancel}>{cancelLabel}</Button>
				<Button variant="danger" loading={loading} onclick={onconfirm}>{confirmLabel}</Button>
			</div>
		</div>
	</div>
{/if}
