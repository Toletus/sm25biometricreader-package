namespace Toletus.SM25.Command.Enums
{
    public enum GDCodes

    {
        GD_DOWNLOAD_SUCCESS = 0xA1,
        GD_NEED_FIRST_SWEEP = 0xFFF1,
        GD_NEED_SECOND_SWEEP = 0xFFF2,
        GD_NEED_THIRD_SWEEP = 0xFFF3,
        GD_NEED_RELEASE_FINGER = 0xFFF4,
        GD_DETECT_FINGER = 0x01,
        GD_NO_DETECT_FINGER = 0x00,
    }
}
