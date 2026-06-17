<script lang="ts">
	let { message, title, className = '' } = $props<{
		message: string;
		title?: string;
		className?: string;
	}>();

	const normalizedMessage = $derived(
		message?.trim() || 'The request could not be completed. Please try again.'
	);
	const isAccessDenied = $derived(
		/\b(403|forbidden|permission|access denied|not authorized)\b/i.test(normalizedMessage)
	);
	const heading = $derived(title ?? (isAccessDenied ? 'Access denied' : 'Request failed'));
	const body = $derived(
		isAccessDenied
			? 'Your account does not have permission for this action or resource. Ask an administrator to grant access if this looks incorrect.'
			: normalizedMessage
	);
</script>

<div class={`error-callout ${className}`} role="alert">
	<div class="error-callout-icon" aria-hidden="true">
		{#if isAccessDenied}
			<svg viewBox="0 0 20 20" class="h-5 w-5" fill="none">
				<path d="M10 2.5 16 5v4.4c0 3.1-1.9 5.9-4.8 7L10 17l-1.2-.6C5.9 15.3 4 12.5 4 9.4V5l6-2.5Z" stroke="currentColor" stroke-width="1.5" />
				<path d="M7.8 10.2h4.4M10 8v4.4" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
			</svg>
		{:else}
			<svg viewBox="0 0 20 20" class="h-5 w-5" fill="none">
				<path d="M10 3 17 16H3L10 3Z" stroke="currentColor" stroke-width="1.5" stroke-linejoin="round" />
				<path d="M10 7.5v4M10 14.2v.1" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" />
			</svg>
		{/if}
	</div>
	<div class="min-w-0">
		<p class="error-callout-title">{heading}</p>
		<p class="error-callout-message">{body}</p>
	</div>
</div>

<style>
	.error-callout {
		display: flex;
		align-items: flex-start;
		gap: 12px;
		border: 1px solid color-mix(in srgb, var(--danger) 28%, var(--border));
		border-radius: 8px;
		background: color-mix(in srgb, var(--danger-subtle) 76%, var(--bg-surface));
		padding: 14px 16px;
		box-shadow: var(--shadow-sm);
	}

	.error-callout-icon {
		display: flex;
		width: 32px;
		height: 32px;
		flex: 0 0 auto;
		align-items: center;
		justify-content: center;
		border-radius: 999px;
		background: color-mix(in srgb, var(--danger) 12%, transparent);
		color: var(--danger);
	}

	.error-callout-title {
		font-size: 14px;
		font-weight: 600;
		color: var(--text-primary);
	}

	.error-callout-message {
		margin-top: 3px;
		font-size: 13px;
		color: var(--text-secondary);
	}
</style>
