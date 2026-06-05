import { redirect } from '@sveltejs/kit';
import { canAll } from '$lib/stores/auth';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ parent }) => {
	const { user } = await parent();
	if (!canAll(user)) throw redirect(302, '/services');
	return {};
};
