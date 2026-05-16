import { redirect } from "next/navigation";

type CredentialPageProps = {
  params: Promise<{
    name: string;
  }>;
};

export default async function CredentialPage({ params }: CredentialPageProps) {
  const { name } = await params;
  redirect(`/?kind=credentials&name=${encodeURIComponent(name)}`);
}
