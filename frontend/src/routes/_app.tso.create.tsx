import { createFileRoute } from '@tanstack/react-router'
import { TsoWizard } from '../components/TsoWizard'

export const Route = createFileRoute('/_app/tso/create')({
  component: TsoCreatePage,
})

function TsoCreatePage() {
  return <TsoWizard />
}
