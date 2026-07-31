import { useParams } from '@tanstack/react-router';

import { EncoderFormPageContent } from './encoder-form-page';

export default function EncoderFormPageDesfireStudio() {
  const { encoderId } = useParams({ from: '/desfire-studio/printing/encoders/$encoderId/edit' });

  return <EncoderFormPageContent encoderId={encoderId} />;
}
