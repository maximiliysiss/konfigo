<script lang="ts">
	let { className = '' } = $props<{ className?: string }>();
</script>

<div class={`overflow-x-auto ${className}`}>
	<table class="min-w-full border-separate border-spacing-0 text-left text-[13px]">
		<slot />
	</table>
</div>

<style>
	:global(.ui-table thead th) {
		position: sticky;
		top: 0;
		height: 40px;
		background: var(--bg-subtle);
		font-size: 12px;
		text-transform: uppercase;
		letter-spacing: 0.08em;
		color: var(--text-tertiary);
		z-index: 1;
	}

	:global(.ui-table tbody tr) {
		transition: background-color 100ms ease;
	}

	:global(.ui-table tbody tr:hover) {
		background: var(--bg-subtle);
	}

	:global(.ui-table tbody td) {
		height: 48px;
		border-bottom: 1px solid var(--border);
		vertical-align: middle;
	}
	
</style>
