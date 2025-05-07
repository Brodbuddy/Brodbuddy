import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { AppRoutes } from '../helpers/appRoutes';


export default function InitiateLoginPage() {
    const [email, setEmail] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const { initiateLogin } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setIsLoading(true);
        setError(null);

        try {
            const success = await initiateLogin({ email });
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
                <div className="bg-accent p-4 text-center">
                    <h1 className="text-xl font-medium text-primary">Login</h1>
                </div>

                <div className="p-6">
                    <div className="mb-6 text-center">
                        <h2 className="mb-2 text-lg font-medium text-primary">Email Verification</h2>
                        <p className="text-center text-primary">
                            Enter your email address to receive a verification code
                        </p>
                    </div>

                    {error && (
                        <div className="mb-4 rounded bg-red-100 p-3 text-red-700">
                            {error}
                        </div>
                    )}

                    <form onSubmit={handleSubmit}>
                        <div className="mb-4">

                            <input
                                type="email"
                                id="email"
                                placeholder="your@email.com"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                required
                                disabled={isLoading}
                                className="w-full rounded-md border-border-brown p-3 border focus:outline-none focus:ring-1 focus:ring-border-brown"
                            />
                        </div>

                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full rounded-md bg-accent p-3 text-primary hover:opacity-90 focus:outline-none disabled:opacity-50"
                        >
                            {isLoading ? 'Sending...' : 'Send Verification Code'}
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