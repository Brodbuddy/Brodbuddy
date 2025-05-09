import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button";
import * as yup from 'yup';
import { AppRoutes } from '@/helpers';

// validation schema Yup
const validationSchema = yup.object({
    email: yup
        .string()
        .required('Email is required')
        .email('Please enter a valid email')
});

type FormValues = {
    email: string;
};

export default function InitiateLoginPage() {
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const { initiateLogin } = useAuth();
    const navigate = useNavigate();

    const { register, handleSubmit, formState: { errors } } = useForm<FormValues>({
        defaultValues: {
            email: ''
        },
        resolver: yupResolver(validationSchema)
    });

    const onSubmit = async (data: FormValues) => {
        setIsLoading(true);
        setError(null);

        try {
            const success = await initiateLogin({ email: data.email });
            if (success) {
                navigate(AppRoutes.verifyLogin);
            } else {
                setError('Failed to send verification code. Please try again.');
            }
        } catch (err) {
            setError('An error occurred. Please try again later.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-bg-cream p-4">
            <div className="w-full max-w-md overflow-hidden rounded-lg bg-bg-white shadow-md">
                <div className="bg-accent-foreground p-4 text-center">
                    <h1 className="text-xl font-medium text-primary-foreground">Login</h1>
                </div>

                <div className="p-6">
                    <div className="mb-6 text-center">
                        <h2 className="mb-2 text-lg font-medium text-accent-foreground">Email Verification</h2>
                        <p className="text-center text-foreground">
                            Enter your email address to receive a verification code
                        </p>
                    </div>

                    {error && (
                        <div className="mb-4 rounded bg-red-100 p-3 text-red-700">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit(onSubmit)}>
                        <div className="mb-4">
                            <Input
                                type="email"
                                id="email"
                                placeholder="your@email.com"
                                {...register("email")}
                                disabled={isLoading}
                                className="border-border-brown focus:ring-accent-foreground"
                            />
                            {errors.email && (
                                <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>
                            )}
                        </div>

                        <Button
                            type="submit"
                            disabled={isLoading}
                            className="w-full bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
                        >
                            {isLoading ? 'Sending...' : 'Send Verification Code'}
                        </Button>
                    </form>

                    <div className="mt-6 text-center text-sm text-foreground">
                        If you experience any issues, please contact support.
                    </div>
                </div>
            </div>
        </div>
    );
}