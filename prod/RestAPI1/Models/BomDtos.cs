namespace RestAPI1.Models;   // ← použij stejný namespace jako ostatní modely (Models/)

public record BomItem(
    string code,
    int quantity,
    string attr1,
    string attr2,
    string attr3,
    string attr4,
    string attr5,
    string attr6,
    string attr7
);

public record BomRequest(
    List<BomItem> items
);