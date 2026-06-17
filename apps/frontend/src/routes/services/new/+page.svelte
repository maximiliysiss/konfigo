<script lang="ts">
	import { goto } from '$app/navigation';
	import { apiRequest, getApiErrorMessage } from '$lib/api';
	import { queueToast } from '$lib/stores/toast';
	import Button from '$lib/components/ui/Button.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import ErrorCallout from '../../../components/ui/ErrorCallout.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Textarea from '$lib/components/ui/Textarea.svelte';

	type ServiceDetail = {
		id: string;
	};

	let name = $state('');
	let description = $state('');
	let repositoryUrl = $state('');
	let contactEmail = $state('');
	let saving = $state(false);
	let error = $state('');

	function validateForm(): string | null {
		if (!name.trim()) return 'Name is required.';
		if (repositoryUrl.trim()) {
			try {
				new URL(repositoryUrl.trim());
			} catch {
				return 'Repository URL must be a valid URL.';
			}
		}
		if (contactEmail.trim() && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(contactEmail.trim())) {
			return 'Contact Email must be a valid email address.';
		}
		return null;
	}

	async function submit() {
		error = '';
		const validationError = validateForm();
		if (validationError) {
			error = validationError;
			return;
		}

		saving = true;
		try {
			const service = await apiRequest<ServiceDetail>('/services', {
				method: 'POST',
				body: JSON.stringify({
					name: name.trim(),
					description,
					repositoryUrl: repositoryUrl.trim(),
					contactEmail: contactEmail.trim()
				})
			});

			queueToast('Service created successfully.', 'success');
			await goto(`/services/${service.id}#members`);
		} catch (e) {
			error = getApiErrorMessage(e, 'Failed to create service');
		} finally {
			saving = false;
		}
	}

	async function cancel() {
		await goto('/services');
	}
</script>

<section class="space-y-6">
	<div class="page-header border-l-2 border-[var(--ink)] pl-5">
		<div>
			<p class="section-label">Provision</p>
			<h1 class="page-title mt-2">Create service</h1>
			<p class="page-subtitle">Define the service identity, repository details, and team contact information.</p>
		</div>
	</div>

	<div class="max-w-[640px]">
		<Card>
			<div class="space-y-8">
				<section class="space-y-5">
					<div>
						<p class="section-label">Identity</p>
						<h2 class="mt-2 text-[20px] font-semibold">Core details</h2>
					</div>
					<div class="space-y-5">
						<Input id="name" label="Name *" placeholder="Service name" bind:value={name} />
						<Textarea id="description" label="Description" placeholder="Describe what this service owns or exposes." bind:value={description} />
					</div>
				</section>

				<section class="space-y-5">
					<div>
						<p class="section-label">Links</p>
						<h2 class="mt-2 text-[20px] font-semibold">Repository metadata</h2>
					</div>
					<div class="space-y-5">
						<Input id="repo" label="Repository URL" placeholder="https://gitlab.example.com/group/service" bind:value={repositoryUrl} />
						<Input id="contact" label="Contact Email" type="email" placeholder="team@example.com" bind:value={contactEmail} />
					</div>
				</section>

				{#if error}
					<ErrorCallout message={error} />
				{/if}

				<div class="flex justify-end gap-3">
					<Button variant="ghost" onclick={cancel}>Cancel</Button>
					<Button onclick={submit} loading={saving} disabled={!name.trim()}>Create service</Button>
				</div>
			</div>
		</Card>
	</div>
</section>
