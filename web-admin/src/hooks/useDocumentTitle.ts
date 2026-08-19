import { useEffect } from 'react';

/** Đặt `document.title` theo trang hiện tại, giúp phân biệt tab khi mở nhiều trang cùng lúc. */
export function useDocumentTitle(title: string): void {
  useEffect(() => {
    document.title = `${title} — DAT.ProductionMES`;
  }, [title]);
}
