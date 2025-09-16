import React from "react";
import { EmailVerificationBanner } from "../auth/EmailVerificationBanner";

interface DashboardLayoutProps {
  children: React.ReactNode;
}

export const DashboardLayout: React.FC<DashboardLayoutProps> = ({
  children,
}) => {
  return (
    <div className='min-h-screen bg-gray-50'>
      <EmailVerificationBanner />
      {children}
    </div>
  );
};
