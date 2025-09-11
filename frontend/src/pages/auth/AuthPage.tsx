import React, { useState, useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { LoginForm } from "../../components/auth/LoginForm";
import { RegisterForm } from "../../components/auth/RegisterForm";
import { ROUTES } from "../../constants";

export const AuthPage: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  // Initialize based on current route
  const [isLogin, setIsLogin] = useState(location.pathname === ROUTES.LOGIN);

  // Update state when route changes
  useEffect(() => {
    setIsLogin(location.pathname === ROUTES.LOGIN);
  }, [location.pathname]);

  const handleToggleToRegister = () => {
    setIsLogin(false);
    navigate(ROUTES.REGISTER, { replace: true });
  };

  const handleToggleToLogin = () => {
    setIsLogin(true);
    navigate(ROUTES.LOGIN, { replace: true });
  };

  return (
    <div className='min-h-screen bg-gradient-to-br from-primary-50 to-purple-50 flex items-center justify-center p-4'>
      <div className='w-full max-w-md'>
        {isLogin ? (
          <LoginForm onToggleForm={handleToggleToRegister} />
        ) : (
          <RegisterForm onToggleForm={handleToggleToLogin} />
        )}
      </div>
    </div>
  );
};
