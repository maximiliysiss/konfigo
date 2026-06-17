<script lang="ts">
	import Button from './Button.svelte';

	let {
		title = '',
		open = false,
		widthClass = 'max-w-[480px]',
		onclose,
		footer = true,
		children,
		footerContent
	} = $props<{
		title?: string;
		open?: boolean;
		widthClass?: string;
		onclose?: () => void;
		footer?: boolean;
		children?: import('svelte').Snippet;
		footerContent?: import('svelte').Snippet;
	}>();
</script>

{#if open}
	<div class="fixed inset-0 z-50 flex justify-end bg-black/40 backdrop-blur-[4px]">
		<div class={`h-full w-full ${widthClass} translate-x-0 border-l border-[var(--border)] bg-[var(--bg-surface)] shadow-[var(--shadow-md)] transition-transform duration-200 ease-out`}>
			<div class="flex h-full flex-col">
				<header class="flex items-center justify-between border-b border-[var(--border)] px-6 py-5">
					<h2 class="text-[20px] font-semibold">{title}</h2>
					<Button variant="ghost" size="sm" onclick={onclose} className="w-8 px-0" ariaLabel="Close">
						<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
							<path d="M4 4l8 8M12 4l-8 8" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" />
						</svg>
					</Button>
				</header>
				<div class="flex-1 overflow-y-auto px-6 py-5">
					{@render children?.()}
				</div>
				{#if footer}
					<footer class="sticky bottom-0 border-t border-[var(--border)] bg-[var(--bg-surface)] px-6 py-4">
						{@render footerContent?.()}
					</footer>
				{/if}
			</div>
		</div>
	</div>
{/if}
