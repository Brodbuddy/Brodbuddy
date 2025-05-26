import { useState } from "react";
import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { toast } from "sonner";
import { Loader2, Check } from "lucide-react";
import { useAtom } from "jotai";
import { api, analyzersAtom } from "../import";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";

const activationSchema = yup.object({
  activationCode: yup
    .string()
    .required("Activation code is required")
    .matches(/^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$/, "Activation code must be in the format XXXX-XXXX-XXXX"),
  nickname: yup
    .string()
    .optional()
    .transform(value => value === "" ? undefined : value)
});

type ActivationFormValues = {
  activationCode: string;
  nickname?: string;
};

interface ActivationFormProps {
  onSuccess?: () => void;
  className?: string;
}

function ActivationForm({ onSuccess, className }: ActivationFormProps) {
  const [analyzers, setAnalyzers] = useAtom(analyzersAtom);
  const [isActivating, setIsActivating] = useState(false);
  const [activationSuccess, setActivationSuccess] = useState(false);

  const form = useForm<ActivationFormValues>({
    resolver: yupResolver(activationSchema) as any,
    defaultValues: {
      activationCode: "",
      nickname: undefined
    }
  });

  const formatActivationCode = (value: string) => {
    const cleaned = value.replace(/[^A-Z0-9]/gi, "").toUpperCase();

    if (cleaned.length <= 4) {
      return cleaned;
    } else if (cleaned.length <= 8) {
      return `${cleaned.slice(0, 4)}-${cleaned.slice(4)}`;
    } else {
      return `${cleaned.slice(0, 4)}-${cleaned.slice(4, 8)}-${cleaned.slice(8, 12)}`;
    }
  };

  const onSubmit = async (data: ActivationFormValues) => {
    setIsActivating(true);

    try {
      const response = await api.analyzer.registerAnalyzer({
        activationCode: data.activationCode,
        nickname: data.nickname
      });

      const newAnalyzer = {
        id: response.data.analyzerId,
        name: response.data.name,
        nickname: response.data.nickname,
        isOwner: response.data.isOwner,
        lastSeen: null,
        firmwareVersion: null,
        hasUpdate: false,
        activatedAt: new Date().toISOString()
      };

      setAnalyzers([...analyzers, newAnalyzer]);
      setActivationSuccess(true);

      toast.success("Device activated successfully!", {
        description: `${data.nickname || response.data.name} is now ready to use`
      });

      setTimeout(() => {
        form.reset();
        setActivationSuccess(false);
        if (onSuccess) onSuccess();
      }, 2000);
    } catch (error: any) {
      toast.error("Failed to activate device", {
        description: error.response?.data?.message || "Please check your activation code and try again"
      });
    } finally {
      setIsActivating(false);
    }
  };

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className={className}>
      <div className="space-y-4">
        <div className="space-y-2">
          <label htmlFor="activationCode" className="text-sm font-medium">
            Activation Code
          </label>
          <Input
            id="activationCode"
            placeholder="XXXX-XXXX-XXXX"
            className="font-mono tracking-wider border border-gray-300 dark:border-gray-600"
            disabled={isActivating || activationSuccess}
            {...form.register("activationCode")}
            onChange={(e) => {
              const formatted = formatActivationCode(e.target.value);
              if (formatted.length <= 14) {
                form.setValue("activationCode", formatted);
              }
            }}
          />
          {form.formState.errors.activationCode && (
            <p className="text-red-500 text-sm">{form.formState.errors.activationCode.message}</p>
          )}
          <p className="text-sm text-muted-foreground">
            Enter the 12-character code from your device packaging
          </p>
        </div>

        <div className="space-y-2">
          <label htmlFor="nickname" className="text-sm font-medium">
            Device Nickname (optional)
          </label>
          <Input
            id="nickname"
            placeholder="Kitchen Counter"
            className="border border-gray-300 dark:border-gray-600"
            disabled={isActivating || activationSuccess}
            {...form.register("nickname")}
          />
          {form.formState.errors.nickname && (
            <p className="text-red-500 text-sm">{form.formState.errors.nickname.message}</p>
          )}
          <p className="text-sm text-muted-foreground">
            Choose a name to identify this device
          </p>
        </div>

        <Button
          type="submit"
          disabled={isActivating || activationSuccess}
          className="w-full bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white mt-4"
        >
          {isActivating ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin"/>
              Activating...
            </>
          ) : activationSuccess ? (
            <>
              <Check className="mr-2 h-4 w-4"/>
              Activated!
            </>
          ) : (
            "Activate Device"
          )}
        </Button>
      </div>
    </form>
  );
}

export default ActivationForm;