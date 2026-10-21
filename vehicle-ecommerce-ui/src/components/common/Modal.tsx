import type { ReactNode } from "react";

interface ModalProps {
  open: boolean;
  title: string;
  children: ReactNode;
  onClose: () => void;
}

export default function Modal({
  open,
  title,
  children,
  onClose
}: ModalProps) {
  if (!open) {
    return null;
  }
  return (
    <div role="dialog" aria-modal="true" aria-labelledby="modal-title">
      <div>
        <button type="button" onClick={onClose} aria-label="Close">
          ×
        </button>
        <h2 id="modal-title">{title}</h2>
        <div>{children}</div>
      </div>
    </div>
  );
}
