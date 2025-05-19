import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { useForm } from 'react-hook-form';
import { AppRoutes } from '@/helpers';
import { Button } from "@/components/ui/button";
import { REGEXP_ONLY_DIGITS } from "input-otp";
import {
    InputOTP,
    InputOTPGroup,
    InputOTPSlot,
} from "@/components/ui/input-otp";

type FormValues = {
    code: string;
};

export default function CompleteLoginPage() {
    const [code, setCode] = useState('');
    const [email, setEmail] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const navigate = useNavigate();
    const { completeLogin } = useAuth();

    const { handleSubmit, setValue } = useForm<FormValues>({
        defaultValues: {
            code: ''
        },
        mode: 'onChange'
    });

    useEffect(() => {
        const storedEmail = sessionStorage.getItem('loginEmail');
        if (!storedEmail) {
            navigate(AppRoutes.login);
            return;
        }
        setEmail(storedEmail);
    }, [navigate]);


    const handleCodeChange = (value: string) => {
        setValue('code', value, { shouldValidate: true });
        setCode(value);
    };

    const onSubmit = async (data: FormValues) => {
        setIsLoading(true);
        setError(null);

        try {
            const numericCode = parseInt(data.code, 10);
            if (isNaN(numericCode)) {
                setError('Please enter a valid numeric code');
                setIsLoading(false);
                return;
            }

            const success = await completeLogin(numericCode);
            if (!success) {
                setError('Invalid verification code. Please try again.');
            }
        } catch {
            setError('An error occurred. Please try again later.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="flex min-h-screen items-center justify-center bg-bg-cream p-4">
            <div className="w-full max-w-md overflow-hidden rounded-lg bg-bg-white shadow-md">
                <div className="bg-accent-foreground p-4 text-center">
                    <h1 className="text-xl font-medium text-primary-foreground">Enter Verification Code</h1>
                </div>

                <div className="p-6">
                    <div className="mb-6 text-center">
                        <p className="text-center text-foreground">
                            We sent a code to {email}.
                        </p>
                        <p className="text-center text-foreground">
                            Please enter it below to complete login.
                        </p>
                    </div>

                    {error && (
                        <div className="mb-4 rounded bg-red-100 p-3 text-red-700">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit(onSubmit)}>
                        <div className="mb-6">
                            <div className="flex justify-center">
                                <InputOTP
                                    maxLength={6}
                                    pattern={REGEXP_ONLY_DIGITS}
                                    value={code}
                                    onChange={handleCodeChange}
                                    disabled={isLoading}
                                    className="gap-2 scale-110"
                                >
                                    <InputOTPGroup className="">
                                        <InputOTPSlot index={0} className="w-10 h-12 text-lg border-border-brown" />
                                        <InputOTPSlot index={1} className="w-10 h-12 text-lg border-border-brown" />
                                        <InputOTPSlot index={2} className="w-10 h-12 text-lg border-border-brown" />
                                        <InputOTPSlot index={3} className="w-10 h-12 text-lg border-border-brown" />
                                        <InputOTPSlot index={4} className="w-10 h-12 text-lg border-border-brown" />
                                        <InputOTPSlot index={5} className="w-10 h-12 text-lg border-border-brown" />
                                    </InputOTPGroup>
                                </InputOTP>
                            </div>
                        </div>

                        <Button
                            type="submit"
                            disabled={isLoading || code.length !== 6}
                            className="w-full mb-2 bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
                        >
                            {isLoading ? 'Verifying...' : 'Login'}
                        </Button>

                        <Button
                            variant="ghost"
                            type="button"
                            onClick={() => navigate(AppRoutes.login)}
                            className="w-full mt-2 text-accent-foreground hover:bg-secondary/50"
                        >
                            Back to login page
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