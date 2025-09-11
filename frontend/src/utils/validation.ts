import * as yup from "yup";
import { VALIDATION } from "../constants";

// Common validation schemas
export const validationSchemas = {
  email: yup
    .string()
    .email("Please enter a valid email address")
    .required("Email is required"),

  password: yup
    .string()
    .min(
      VALIDATION.PASSWORD_MIN_LENGTH,
      `Password must be at least ${VALIDATION.PASSWORD_MIN_LENGTH} characters`
    )
    .matches(
      /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
      "Password must contain at least one uppercase letter, one lowercase letter, and one number"
    )
    .required("Password is required"),

  firstName: yup
    .string()
    .min(2, "First name must be at least 2 characters")
    .max(50, "First name must be less than 50 characters")
    .required("First name is required"),

  lastName: yup
    .string()
    .min(2, "Last name must be at least 2 characters")
    .max(50, "Last name must be less than 50 characters")
    .required("Last name is required"),

  organizationName: yup
    .string()
    .min(2, "Organization name must be at least 2 characters")
    .max(
      VALIDATION.ORGANIZATION_NAME_MAX_LENGTH,
      `Organization name must be less than ${VALIDATION.ORGANIZATION_NAME_MAX_LENGTH} characters`
    )
    .required("Organization name is required"),

  workflowName: yup
    .string()
    .min(2, "Workflow name must be at least 2 characters")
    .max(
      VALIDATION.WORKFLOW_NAME_MAX_LENGTH,
      `Workflow name must be less than ${VALIDATION.WORKFLOW_NAME_MAX_LENGTH} characters`
    )
    .required("Workflow name is required"),
};

// Form schemas
export const loginSchema = yup.object({
  email: validationSchemas.email,
  password: yup.string().required("Password is required"),
});

export const registerSchema = yup.object({
  email: validationSchemas.email,
  password: validationSchemas.password,
  firstName: validationSchemas.firstName,
  lastName: validationSchemas.lastName,
  organizationName: validationSchemas.organizationName,
});

export const forgotPasswordSchema = yup.object({
  email: validationSchemas.email,
});

export const resetPasswordSchema = yup.object({
  password: validationSchemas.password,
  confirmPassword: yup
    .string()
    .oneOf([yup.ref("password")], "Passwords must match")
    .required("Please confirm your password"),
});

export const updateProfileSchema = yup.object({
  firstName: validationSchemas.firstName,
  lastName: validationSchemas.lastName,
  timeZone: yup.string().optional(),
});

export const changePasswordSchema = yup.object({
  currentPassword: yup.string().required("Current password is required"),
  newPassword: validationSchemas.password,
  confirmPassword: yup
    .string()
    .oneOf([yup.ref("newPassword")], "Passwords must match")
    .required("Please confirm your new password"),
});
