<script lang="ts">
	import { onMount } from 'svelte';
	import { createLocalUser, deleteLocalUser, fetchLocalUsers, getApiErrorMessage, updateLocalUser } from '$lib/api';
	import type { LocalUserContract, LocalUserRole } from '$lib/api';
	import { user } from '$lib/stores/auth';
	import Button from '$lib/components/ui/Button.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Table from '$lib/components/ui/Table.svelte';
	import Badge from '$lib/components/ui/Badge.svelte';
	import Modal from '$lib/components/ui/Modal.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import ConfirmDialog from '$lib/components/ui/ConfirmDialog.svelte';
	import EmptyState from '$lib/components/ui/EmptyState.svelte';
	import ErrorCallout from '../../components/ui/ErrorCallout.svelte';
	import { showToast } from '$lib/stores/toast';

	let users = $state<LocalUserContract[]>([]);
	let loading = $state(true);
	let error = $state('');

	let createOpen = $state(false);
	let createUsername = $state('');
	let createPassword = $state('');
	let createRole = $state<LocalUserRole>('developer');
	let createError = $state('');
	let creating = $state(false);

	let editingUser = $state<LocalUserContract | null>(null);
	let editRole = $state<LocalUserRole>('developer');
	let editPassword = $state('');
	let editError = $state('');
	let saving = $state(false);

	let deletingUser = $state<LocalUserContract | null>(null);
	let deleting = $state(false);

	async function loadUsers() {
		loading = true;
		error = '';
		try {
			users = await fetchLocalUsers();
		} catch (e) {
			error = getApiErrorMessage(e, 'Failed to load users');
			users = [];
		} finally {
			loading = false;
		}
	}

	onMount(loadUsers);

	function openCreate() {
		createUsername = '';
		createPassword = '';
		createRole = 'developer';
		createError = '';
		createOpen = true;
	}

	async function submitCreate() {
		createError = '';
		if (!createUsername.trim() || !createPassword.trim()) {
			createError = 'Username and password are required';
			return;
		}
		creating = true;
		try {
			const created = await createLocalUser(createUsername.trim(), createPassword, createRole);
			users = [...users, created].sort((a, b) => a.username.localeCompare(b.username));
			createOpen = false;
			showToast('User created', 'success');
		} catch (e) {
			createError = getApiErrorMessage(e, 'Failed to create user');
		} finally {
			creating = false;
		}
	}

	function openEdit(target: LocalUserContract) {
		editingUser = target;
		editRole = target.role;
		editPassword = '';
		editError = '';
	}

	async function submitEdit() {
		if (!editingUser) return;
		editError = '';
		saving = true;
		try {
			const changes: { role?: LocalUserRole; password?: string } = {};
			if (editRole !== editingUser.role) changes.role = editRole;
			if (editPassword.trim()) changes.password = editPassword.trim();

			const updated = await updateLocalUser(editingUser.username, changes);
			users = users.map((u) => (u.username === updated.username ? updated : u));
			editingUser = null;
			showToast('User updated', 'success');
		} catch (e) {
			editError = getApiErrorMessage(e, 'Failed to update user');
		} finally {
			saving = false;
		}
	}

	async function confirmDelete() {
		if (!deletingUser) return;
		deleting = true;
		try {
			await deleteLocalUser(deletingUser.username);
			users = users.filter((u) => u.username !== deletingUser!.username);
			showToast('User deleted', 'success');
			deletingUser = null;
		} catch (e) {
			showToast(getApiErrorMessage(e, 'Failed to delete user'), 'error');
		} finally {
			deleting = false;
		}
	}

	function formatDate(value: string | null): string {
		if (!value) return 'Never';
		return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value));
	}

	const isSelf = (username: string) => $user?.id === username;
</script>

<section class="space-y-6">
	<div class="page-header">
		<div class="max-w-[620px]">
			<p class="section-label">Access control</p>
			<h1 class="page-title mt-2">Users</h1>
			<p class="page-subtitle">Manage local accounts and their roles for username/password sign-in.</p>
		</div>
		<Button size="lg" onclick={openCreate}>Add user</Button>
	</div>

	{#if loading}
		<Card className="animate-pulse">
			<div class="space-y-3">
				{#each Array(4) as _}
					<div class="h-10 w-full rounded bg-[var(--bg-muted)]"></div>
				{/each}
			</div>
		</Card>
	{:else if error}
		<ErrorCallout message={error} />
	{:else if users.length === 0}
		<EmptyState title="No users yet" description="Add a user to let them sign in with a username and password.">
			{#snippet icon()}
				<svg viewBox="0 0 48 48" class="h-12 w-12" fill="none" aria-hidden="true">
					<circle cx="24" cy="18" r="7" stroke="currentColor" stroke-width="2" />
					<path d="M10 38c0-6.6 6.3-12 14-12s14 5.4 14 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
				</svg>
			{/snippet}
			{#snippet action()}
				<Button onclick={openCreate}>Add user</Button>
			{/snippet}
		</EmptyState>
	{:else}
		<Card>
			<Table>
				<thead>
					<tr>
						<th class="h-10 bg-[var(--bg-subtle)] px-4 text-[12px] uppercase tracking-[0.08em] text-[var(--text-tertiary)]">Username</th>
						<th class="h-10 bg-[var(--bg-subtle)] px-4 text-[12px] uppercase tracking-[0.08em] text-[var(--text-tertiary)]">Role</th>
						<th class="h-10 bg-[var(--bg-subtle)] px-4 text-[12px] uppercase tracking-[0.08em] text-[var(--text-tertiary)]">Created</th>
						<th class="h-10 bg-[var(--bg-subtle)] px-4"></th>
					</tr>
				</thead>
				<tbody>
					{#each users as u}
						<tr class="hover:bg-[var(--bg-subtle)]">
							<td class="h-12 border-b border-[var(--border)] px-4 font-mono">
								{u.username}
								{#if isSelf(u.username)}
									<span class="ml-2 text-[11px] text-[var(--text-tertiary)]">(you)</span>
								{/if}
							</td>
							<td class="h-12 border-b border-[var(--border)] px-4">
								<Badge variant={u.role === 'admin' ? 'accent' : 'default'}>{u.role}</Badge>
							</td>
							<td class="h-12 border-b border-[var(--border)] px-4 text-[var(--text-secondary)]">{formatDate(u.createdAt)}</td>
							<td class="h-12 border-b border-[var(--border)] px-4">
								<div class="flex justify-end gap-2">
									<Button variant="secondary" size="sm" onclick={() => openEdit(u)}>Edit</Button>
									<Button
										variant="danger"
										size="sm"
										disabled={isSelf(u.username)}
										onclick={() => (deletingUser = u)}
									>
										Delete
									</Button>
								</div>
							</td>
						</tr>
					{/each}
				</tbody>
			</Table>
		</Card>
	{/if}
</section>

<Modal title="Add user" open={createOpen} onclose={() => (createOpen = false)}>
	<div class="space-y-4">
		<Input id="create-username" label="Username" placeholder="jane" bind:value={createUsername} />
		<Input id="create-password" type="password" label="Password" placeholder="••••••••" bind:value={createPassword} />
		<Select id="create-role" label="Role" bind:value={createRole}>
			<option value="developer">Developer</option>
			<option value="admin">Admin</option>
		</Select>
		{#if createError}
			<p class="text-[13px] text-[var(--danger)]">{createError}</p>
		{/if}
	</div>
	{#snippet footerContent()}
		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => (createOpen = false)}>Cancel</Button>
			<Button loading={creating} onclick={submitCreate}>Create</Button>
		</div>
	{/snippet}
</Modal>

<Modal title="Edit user" open={editingUser !== null} onclose={() => (editingUser = null)}>
	{#if editingUser}
		<div class="space-y-4">
			<p class="text-[13px] text-[var(--text-secondary)]">Editing <span class="font-mono">{editingUser.username}</span></p>
			<Select id="edit-role" label="Role" bind:value={editRole}>
				<option value="developer">Developer</option>
				<option value="admin">Admin</option>
			</Select>
			<Input id="edit-password" type="password" label="New password (optional)" placeholder="Leave blank to keep current password" bind:value={editPassword} />
			{#if editError}
				<p class="text-[13px] text-[var(--danger)]">{editError}</p>
			{/if}
		</div>
	{/if}
	{#snippet footerContent()}
		<div class="flex justify-end gap-3">
			<Button variant="ghost" onclick={() => (editingUser = null)}>Cancel</Button>
			<Button loading={saving} onclick={submitEdit}>Save</Button>
		</div>
	{/snippet}
</Modal>

<ConfirmDialog
	open={deletingUser !== null}
	title="Delete user"
	message={`Are you sure you want to delete "${deletingUser?.username ?? ''}"? This cannot be undone.`}
	confirmLabel="Delete"
	loading={deleting}
	onconfirm={confirmDelete}
	oncancel={() => (deletingUser = null)}
/>
