using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("carts")]
public class Cart
{
    [Key]
    public int Id { get; set; }

    public int User_Id { get; set; }

    public DateTime Created_At { get; set; }

    public virtual ICollection<Cart_Item> Cart_Items { get; set; }
}