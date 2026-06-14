#import <UIKit/UIKit.h>

extern "C"
{
    void _HapticLight()
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
            [gen prepare];
            [gen impactOccurred];
        }
    }

    void _HapticMedium()
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
            [gen prepare];
            [gen impactOccurred];
        }
    }

    void _HapticHeavy()
    {
        if (@available(iOS 10.0, *))
        {
            UIImpactFeedbackGenerator *gen = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
            [gen prepare];
            [gen impactOccurred];
        }
    }

    void _HapticSuccess()
    {
        if (@available(iOS 10.0, *))
        {
            UINotificationFeedbackGenerator *gen = [[UINotificationFeedbackGenerator alloc] init];
            [gen prepare];
            [gen notificationOccurred:UINotificationFeedbackTypeSuccess];
        }
    }

    void _HapticError()
    {
        if (@available(iOS 10.0, *))
        {
            UINotificationFeedbackGenerator *gen = [[UINotificationFeedbackGenerator alloc] init];
            [gen prepare];
            [gen notificationOccurred:UINotificationFeedbackTypeError];
        }
    }
}
