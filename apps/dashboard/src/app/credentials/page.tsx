import { redirect } from "next/navigation";

export default function CredentialsPage() {
  redirect("/?kind=credentials");
}
