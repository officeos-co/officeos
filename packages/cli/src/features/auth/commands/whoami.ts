import { requireContext } from "../../../lib/config-store";
import { print } from "../../../shell/output";
import { getMe } from "../api/auth-api";

export async function whoamiCommand(): Promise<void> {
  const context = await requireContext();
  const me = await getMe(context.apiUrl, context.token);
  print(`${me.email} (${me.id})`);
}
