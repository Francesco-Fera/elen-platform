import React from "react";

interface AlertProps {
  type: "success" | "error" | "warning" | "info";
  title?: string;
  message: string;
  onClose?: () => void;
  className?: string;
}

export const Alert: React.FC<AlertProps> = ({
  type,
  title,
  message,
  onClose,
  className = "",
}) => {
  const typeStyles = {
    success: {
      container: "bg-success-50 border border-success-200",
      icon: "text-success-400",
      title: "text-success-800",
      message: "text-success-700",
      iconPath: "M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z",
    },
    error: {
      container: "bg-error-50 border border-error-200",
      icon: "text-error-400",
      title: "text-error-800",
      message: "text-error-700",
      iconPath:
        "M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z",
    },
    warning: {
      container: "bg-warning-50 border border-warning-200",
      icon: "text-warning-400",
      title: "text-warning-800",
      message: "text-warning-700",
      iconPath:
        "M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z",
    },
    info: {
      container: "bg-blue-50 border border-blue-200",
      icon: "text-blue-400",
      title: "text-blue-800",
      message: "text-blue-700",
      iconPath: "M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z",
    },
  };

  const styles = typeStyles[type];

  return (
    <div className={`rounded-md p-4 ${styles.container} ${className}`}>
      <div className='flex'>
        <div className='flex-shrink-0'>
          <svg
            className={`h-5 w-5 ${styles.icon}`}
            fill='currentColor'
            viewBox='0 0 20 20'
          >
            <path fillRule='evenodd' d={styles.iconPath} clipRule='evenodd' />
          </svg>
        </div>
        <div className='ml-3 flex-1'>
          {title && (
            <h3 className={`text-sm font-medium ${styles.title}`}>{title}</h3>
          )}
          <div className={`text-sm ${title ? "mt-1" : ""} ${styles.message}`}>
            {message}
          </div>
        </div>
        {onClose && (
          <div className='ml-auto pl-3'>
            <div className='-mx-1.5 -my-1.5'>
              <button
                onClick={onClose}
                className={`inline-flex rounded-md p-1.5 ${styles.icon} hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-green-50 focus:ring-green-600`}
              >
                <svg
                  className='h-5 w-5'
                  viewBox='0 0 20 20'
                  fill='currentColor'
                >
                  <path
                    fillRule='evenodd'
                    d='M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z'
                    clipRule='evenodd'
                  />
                </svg>
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
