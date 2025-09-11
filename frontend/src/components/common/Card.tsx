import React from "react";

interface CardProps {
  children: React.ReactNode;
  className?: string;
  padding?: boolean;
}

interface CardHeaderProps {
  children: React.ReactNode;
  className?: string;
}

interface CardBodyProps {
  children: React.ReactNode;
  className?: string;
}

interface CardFooterProps {
  children: React.ReactNode;
  className?: string;
}

export const Card: React.FC<CardProps> & {
  Header: React.FC<CardHeaderProps>;
  Body: React.FC<CardBodyProps>;
  Footer: React.FC<CardFooterProps>;
} = ({ children, className = "", padding = true }) => {
  return <div className={`card ${className}`}>{children}</div>;
};

Card.Header = ({ children, className = "" }: CardHeaderProps) => (
  <div className={`card-header ${className}`}>{children}</div>
);

Card.Body = ({ children, className = "" }: CardBodyProps) => (
  <div className={`card-body ${className}`}>{children}</div>
);

Card.Footer = ({ children, className = "" }: CardFooterProps) => (
  <div className={`card-footer ${className}`}>{children}</div>
);
