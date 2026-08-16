export default function Pagination({
  currentPage,
  totalCount,
  pageSize,
  onPageChange,
}: {
  currentPage: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const pageLinks = Array.from({ length: totalPages }, (_, i) => i + 1)
    .filter((page) => page === 1 || page === totalPages || Math.abs(page - currentPage) <= 2)
    .reduce<(number | string)[]>((acc, page) => {
      if (acc.length > 0 && page - (acc[acc.length - 1] as number) > 1) {
        acc.push("...");
      }
      acc.push(page);
      return acc;
    }, []);

  const goTo = (page: number) => {
    if (page < 1 || page > totalPages || page === currentPage) return;
    onPageChange(page);
  };

  return (
    <ul className="pagination">
      <li
        className={`page-item service prev-page ${
          currentPage === 1 ? "disabled" : ""
        }`}
      >
        <a
          className="page-link"
          href="#"
          onClick={(e) => {
            e.preventDefault();
            goTo(currentPage - 1);
          }}
        >
          Prev
        </a>
      </li>

      {pageLinks.map((page, i) =>
        page === "..." ? (
          <li key={`gap-${i}`} className="page-item no-link">
            <a className="page-link">...</a>
          </li>
        ) : (
          <li
            key={page}
            className={`page-item ${page === currentPage ? "active" : ""}`}
          >
            <a
              className="page-link"
              href="#"
              onClick={(e) => {
                e.preventDefault();
                goTo(page as number);
              }}
            >
              {page}
            </a>
          </li>
        )
      )}

      <li
        className={`page-item service next-page ${
          currentPage === totalPages ? "disabled" : ""
        }`}
      >
        <a
          className="page-link"
          href="#"
          onClick={(e) => {
            e.preventDefault();
            goTo(currentPage + 1);
          }}
        >
          Next
        </a>
      </li>
    </ul>
  );
}
