"""add description to product

Revision ID: 002_add_product_description
Revises: 001_create_products
Create Date: 2026-04-28
"""

from __future__ import annotations

from alembic import op
import sqlalchemy as sa


revision = "002_add_product_description"
down_revision = "001_create_products"
branch_labels = None
depends_on = None


def upgrade() -> None:
    # SQLite needs batch mode for ALTERs that change constraints/nullability.
    with op.batch_alter_table("products") as batch_op:
        batch_op.add_column(sa.Column("description", sa.String(length=500), nullable=True))

    op.execute("UPDATE products SET description = 'No description' WHERE description IS NULL")

    with op.batch_alter_table("products") as batch_op:
        batch_op.alter_column("description", existing_type=sa.String(length=500), nullable=False)


def downgrade() -> None:
    with op.batch_alter_table("products") as batch_op:
        batch_op.drop_column("description")
