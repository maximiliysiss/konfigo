<script lang="ts">
	import type { AppToast } from '$lib/stores/toast';

	let { toast } = $props<{ toast: AppToast }>();

	const styles = {
		success: 'border-[var(--success)] bg-[var(--bg-surface)]',
		error: 'border-[var(--danger)] bg-[var(--bg-surface)]',
		warning: 'border-[var(--warning)] bg-[var(--bg-surface)]',
		info: 'border-[var(--accent)] bg-[var(--bg-surface)]'
	} satisfies Record<AppToast['type'], string>;
	const bars = {
		success: 'bg-[var(--success)]',
		error: 'bg-[var(--danger)]',
		warning: 'bg-[var(--warning)]',
		info: 'bg-[var(--accent)]'
	} satisfies Record<AppToast['type'], string>;
</script>

<div class={`relative w-80 overflow-hidden rounded-[var(--radius-md)] border-l-4 border border-[var(--border)] p-4 shadow-[var(--shadow-md)] ${styles[toast.type as AppToast['type']]}`}>
	<p class="pr-6 text-[13px] text-[var(--text-primary)]">{toast.message}</p>
	<div class="absolute bottom-0 left-0 h-[2px] w-full bg-[var(--bg-muted)]">
		<div class={`h-full origin-left animate-[toast-progress_4s_linear_forwards] ${bars[toast.type as AppToast['type']]}`}></div>
	</div>
</div>

<style>
	@keyframes toast-progress {
		from {
			transform: scaleX(1);
		}
		to {
			transform: scaleX(0);
		}
	}
</style>
