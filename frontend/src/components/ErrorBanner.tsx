interface ErrorBannerProps {
  message: string
}

/** A general error message. role="alert" makes screen readers announce it. */
export default function ErrorBanner({ message }: ErrorBannerProps) {
  return (
    <div className="error-banner" role="alert">
      {message}
    </div>
  )
}
