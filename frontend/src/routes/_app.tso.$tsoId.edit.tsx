import { createFileRoute } from '@tanstack/react-router'
import { TsoWizard } from '../components/TsoWizard'

export const Route = createFileRoute('/_app/tso/$tsoId/edit')({
  component: TsoEditPage,
})

function TsoEditPage() {
  const { tsoId } = Route.useParams()
  return <TsoWizard editId={Number(tsoId)} />
}
