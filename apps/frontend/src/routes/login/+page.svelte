<script lang="ts">
	import { page } from '$app/stores';
	import { buildBackendUrl } from '$lib/api';

	const returnUrl = $derived.by(() => {
		const value = $page.url.searchParams.get('returnUrl');
		return value && value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\')
			? value
			: '/services';
	});

	const loginHref = $derived(buildBackendUrl(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`));
</script>

<svelte:head>
	<title>Sign in - Konfigo</title>
</svelte:head>

<main class="login-background">
	<div class="w-full max-w-[420px] rounded-[8px] border border-[var(--border)] bg-[var(--bg-surface)] p-7 shadow-[var(--shadow-md)]">
		<div class="mb-7 flex items-center gap-3">
			<img src="/app-icon.png" alt="" class="h-12 w-12 rounded-[10px] border border-[var(--border)] bg-[var(--bg-elevated)] object-cover" />
			<div>
				<p class="section-label">Konfigo</p>
				<h1 class="mt-1 text-[22px] font-semibold text-[var(--text-primary)]">Sign in</h1>
			</div>
		</div>
		<p class="text-[14px] text-[var(--text-secondary)]">
			Use your JumpCloud account to enter the configuration control plane.
		</p>

		<a class="login-sso-button mt-6" href={loginHref}>
			<svg viewBox="0 0 16 16" class="h-4 w-4" fill="none" aria-hidden="true">
				<path
					d="M8 1l5 2v5c0 4-2.5 5.5-5 7-2.5-1.5-5-3-5-7V3l5-2Z"
					stroke="currentColor"
					stroke-width="1.5"
					stroke-linejoin="round"
					stroke-linecap="round"
				/>
			</svg>
			<span>Login by SSO</span>
		</a>
	</div>
</main>
