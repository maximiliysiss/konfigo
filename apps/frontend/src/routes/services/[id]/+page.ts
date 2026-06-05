import { redirect } from '@sveltejs/kit';
import { canChange } from '$lib/stores/auth';
import type { PageLoad } from './$types';

export const load: PageLoad = async ({ parent }) => {
	const { user } = await parent();
	if (!canChange(user)) throw redirect(302, '/services');
	return {};
};
