<script lang="ts">
	let { size = 16, className = '' } = $props<{ size?: 16 | 20 | 24; className?: string }>();
</script>

<span
	class={`inline-block animate-spin rounded-full border-2 border-[color:color-mix(in_srgb,var(--accent)_22%,transparent)] border-t-[var(--accent)] ${className}`}
	style={`width:${size}px;height:${size}px;`}
	aria-hidden="true"
></span>
