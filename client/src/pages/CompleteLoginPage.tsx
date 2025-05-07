import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AppRoutes } from '../helpers/appRoutes';
import { REGEXP_ONLY_DIGITS } from "input-otp";
import {
    InputOTP,
    InputOTPGroup,
    InputOTPSlot,
} from "@/components/ui/input-otp";

export default function CompleteLoginPage() {
    const [code, setCode] = useState('');
    const [email, setEmail] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();
    const { completeLogin } = useAuth();

    useEffect(() => {
        const storedEmail = sessionStorage.getItem('loginEmail');
        if (!storedEmail) {
            navigate(AppRoutes.login);
            return;
        }
        setEmail(storedEmail);
    }, [navigate]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError(null);

        try {
            const numericCode = parseInt(code, 10);
            if (isNaN(numericCode)) {
                setError('Please enter a valid numeric code');
                setIsLoading(false);
                return;
            }

            const success = await completeLogin(numericCode);
            if (!success) {
                setError('Invalid verification code. Please try again.');
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
                <div className="bg-accent p-4 text-center">
                    <h1 className="text-xl font-medium text-primary">Enter Verification Code</h1>
                </div>

                <div className="p-6">
                    <div className="mb-6 text-center">
                        <p className="text-center text-primary">
                            We sent a code to {email}.
                        </p>
                        <p className="text-center text-primary">
                            Please enter it below to complete login.
                        </p>
                    </div>

                    {error && (
                        <div className="mb-4 rounded bg-red-100 p-3 text-red-700">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        <div className="mb-6">

                            <div className="flex justify-center">
                                <InputOTP
                                    maxLength={6}
                                    pattern={REGEXP_ONLY_DIGITS}
                                    value={code}
                                    onChange={setCode}
                                    disabled={isLoading}
                                    className="gap-2 scale-110"
                                >
                                    <InputOTPGroup className="">
                                        <InputOTPSlot index={0} className="w-10 h-12 text-lg" />
                                        <InputOTPSlot index={1} className="w-10 h-12 text-lg" />
                                        <InputOTPSlot index={2} className="w-10 h-12 text-lg" />
                                        <InputOTPSlot index={3} className="w-10 h-12 text-lg" />
                                        <InputOTPSlot index={4} className="w-10 h-12 text-lg" />
                                        <InputOTPSlot index={5} className="w-10 h-12 text-lg" />
                                    </InputOTPGroup>
                                </InputOTP>
                            </div>
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading || code.length !== 6}
                            className="w-full rounded-md bg-accent p-3 text-primary hover:opacity-90 focus:outline-none disabled:opacity-50 mb-2"
                        >
                            {isLoading ? 'Verifying...' : 'Login'}
                        </button>

                        <button
                            type="button"
                            onClick={() => navigate(AppRoutes.login)}
                            className="w-full text-primary hover:underline focus:outline-none mt-2"
                        >
                            Tilbage til login
                        </button>
                    </form>

                    <div className="mt-6 text-center text-sm text-primary">
                        If you experience any issues, please contact support.
                    </div>
                </div>
            </div>
        </div>
    );
}